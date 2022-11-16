package main

import (
	"context"
	"encoding/base64"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"log"
	"math/rand"
	"net"
	"net/http"
	"os"
	"strings"
	"time"

	"github.com/AzureAD/microsoft-authentication-library-for-go/apps/confidential"
)

type AuthResponse struct {
	AccessToken string `json:"access_token"`
	ExpiresOn   int64  `json:"expires_on"`
}

type AuthRequest struct {
	Scopes []string
}

type Msi2GoFlags struct {
	TenantId *string
	ClientId *string
	ClientSecret *string
}

const microsoftAuthorityHost = "https://login.microsoftonline.com/"
const defaultScope = "https://management.core.windows.net/.default"
const apiVersion = "2017-09-01"

func writeAuthResponse(w http.ResponseWriter, r *AuthRequest, client *confidential.Client) {
	result, err := client.AcquireTokenSilent(context.Background(), r.Scopes)
	if err != nil {
		result, err = client.AcquireTokenByCredential(context.Background(), r.Scopes)
		if err != nil {
			fmt.Fprintf(w, "AcquireTokenByCredential() error: %w", err)
		}
	}

	response := AuthResponse{AccessToken: result.AccessToken, ExpiresOn: result.ExpiresOn.Unix()}

	jsonResponse, err := json.MarshalIndent(response, "", "  ")
	w.Header().Add("Content-Type", "application/json")
	fmt.Fprint(w, string(jsonResponse))
}

func makeAuthHandler(client *confidential.Client, secret string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		authRequest, err := validateRequest(r, secret)

		if err != nil {
			w.WriteHeader(http.StatusBadRequest)
			fmt.Fprintf(w, err.Error())

			return
		}

		writeAuthResponse(w, authRequest, client)
	}
}

func validateRequest(r *http.Request, secret string) (*AuthRequest, error) {

	// validate the metadata header is present
	// metadataHeaders := r.Header["metadata"]

	// if len(metadataHeaders) == 0 {
	// 	return nil, errors.New("Invalid Metadata")
	// }

	// validate the secret header is present only once
	secretHeader := r.Header.Get("secret")

	if secretHeader == "" {
		return nil, errors.New("No Secret")
	}

	if secretHeader != secret {
		return nil, errors.New("Invalid Secret")
	}

	// validate the api version matches the expected
	apiVersionParams := r.URL.Query()["api-version"]

	if len(apiVersionParams) == 0 {
		return nil, errors.New("Invalid api-version")
	}

	// get the resource from the query parameters
	resourceParams := r.URL.Query()["resource"]

	if len(resourceParams) == 0 {
		return nil, errors.New("Invalid Resource")
	}

	var scope string

	if strings.HasSuffix(resourceParams[0], "/") {
		scope = resourceParams[0] + ".default"
	} else {
		scope = resourceParams[0] + "/.default"
	}

	return &AuthRequest{Scopes: []string{scope}}, nil
}

func processFlags() (*Msi2GoFlags, error) {
	flags := Msi2GoFlags{}

	flags.TenantId = flag.String("tenantid", "", "Tenant id of the development managed idenitty account")

	flags.ClientId = flag.String("clientid", "", "Client id (application id) of the development managed idenitty account")

	flags.ClientSecret = flag.String("secret", "", "Client secret of the development managed idenitty account")

	return &flags, nil
}

func main() {

	tenantId := os.Args[1]
	clientId := os.Args[2]
	secret := os.Args[3]

	authority := microsoftAuthorityHost + tenantId

	cred, err := confidential.NewCredFromSecret(secret)

	if err != nil {
		return
	}

	client, err := confidential.New(clientId, cred, confidential.WithAuthority(authority))

	if err != nil {
		return
	}

	secretBytes := make([]byte, 8)
	rand.Seed(time.Now().UnixNano())
	rand.Read(secretBytes)

	secretHeader := base64.RawURLEncoding.EncodeToString(secretBytes)

	handler := makeAuthHandler(&client, secretHeader)

	http.HandleFunc("/oauth2/v2.0/token", handler)

	listener, err := net.Listen("tcp", ":0")

	if err != nil {
		panic(err)
	}

	fmt.Println("Managed identity endpoint started. To configure your process to authenticate run the following commands to setup the environment:")

	fmt.Println("")

	fmt.Printf("set MSI_ENDPOINT=http://localhost:%d/oauth2/v2.0/token", listener.Addr().(*net.TCPAddr).Port)

	fmt.Println("")

	fmt.Printf("set MSI_SECRET=%s", secretHeader)

	log.Fatal(http.Serve(listener, nil))
}
