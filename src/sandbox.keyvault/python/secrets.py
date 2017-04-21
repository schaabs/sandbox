from key_vault_agent import KeyVaultAgent


class SecretsAgent(KeyVaultAgent):
    def get_secret(self):
        self.data_client.restore_secret()
        pass
