from azure.keyvault.models import SasDefinitionCreateParameters

class _SasConst(object):
    VALIDITY_PERIOD = 'validityPeriod'
    PERMISSIONS = 'signedPermissions'
    RESOURCE_TYPES = 'signedResourceTypes'
    SAS_TYPE = 'sasType'
    SAS_TYPE_ACCOUNT = 'account'
    SAS_TYPE_SERVICE = 'service'
    PROTOCOLS = 'signedProtocols'
    POLICY = 'signedIdentifier'
    IP_RANGE = 'signedIp'
    API_VERSION = 'apiVersion'
    SERVICE_TYPE = 'serviceSasType'
    SERVICES = 'signedServices'
    SERVICE_TYPE_BLOB = 'blob'
    SERVICE_TYPE_FILE = 'file'
    SERVICE_TYPE_TABLE = 'table'
    SERVICE_TYPE_QUEUE = 'queue'
    SERVICE_TYPE_CONTAINER = SERVICE_TYPE_BLOB
    SERVICE_TYPE_SHARE = SERVICE_TYPE_FILE
    BLOB_NAME = 'blobName'
    CONTAINER_NAME = 'containerName'
    FILE_NAME = 'fileName'
    SHARE_NAME = 'shareName'
    TABLE_NAME = 'tableName'
    QUEUE_NAME = 'queueName'
    CACHE_CONTROL = 'rscc'
    CONTENT_DISPOSITION = 'rscd'
    CONTENT_LANGUAGE = 'rscl'
    CONTENT_ENCODING = 'rsce'
    CONTENT_TYPE = 'rsct'

class _SasDefinitionParameters(dict):
    def __init__(self, validity_period, permissions, resource_types, sas_type, https_only=False, policy=None, ip_range=None, api_version=None):
        self[_SasConst.VALIDITY_PERIOD] = validity_period
        self[_SasConst.PERMISSIONS] = permissions
        self[_SasConst.SAS_TYPE] = sas_type
        self[_SasConst.PROTOCOLS] = 'https' if https_only else 'http,https'
        if resource_types:
            self[_SasConst.RESOURCE_TYPES] = resource_types
        if policy:
            self[_SasConst.POLICY] = policy
        if ip_range:
            self[_SasConst.IP_RANGE] = ip_range
        if api_version:
            self[_SasConst.API_VERSION] = api_version
            

class _ServiceSasDefinitionParameters(_SasDefinitionParameters):
    def __init__(self, validity_period, permissions, resource_types, service_type, https_only=False, policy=None, ip_range=None, api_version=None):
        super(_ServiceSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                              permissions=permissions,
                                                              resource_types=resource_types,
                                                              sas_type=_SasConst.SAS_TYPE_SERVICE,
                                                              https_only=https_only,
                                                              policy=policy,
                                                              ip_range=ip_range,
                                                              api_version=api_version)
        # todo validate resource_types
        self[_SasConst.SERVICE_TYPE] = service_type


class _ContentServiceSasDefinitionParameters(_ServiceSasDefinitionParameters):
    def __init__(self, validity_period, permissions, resource_types, service_type, https_only=False, policy=None, ip_range=None, api_version=None,
                 cache_control=None, content_disposition=None, content_language=None, content_encoding=None, content_type=None):
        super(_ContentServiceSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                              permissions=permissions,
                                                              resource_types=resource_types,
                                                              service_type=service_type,
                                                              https_only=https_only,
                                                              policy=policy,
                                                              ip_range=ip_range,
                                                              api_version=api_version)
        if cache_control:
            self[_SasConst.CACHE_CONTROL] = cache_control
        if content_disposition:
            self[_SasConst.CONTENT_DISPOSITION] = content_disposition
        if content_language:
            self[_SasConst.CONTENT_LANGUAGE] = content_language
        if content_encoding:
            self[_SasConst.CONTENT_ENCODING] = content_encoding
        if cache_control:
            self[_SasConst.CONTENT_TYPE] = content_type


class BlobSasDefinitionParameters(_ContentServiceSasDefinitionParameters):
    def __init__(self, blob_name, validity_period, permissions, https_only=False, policy=None, ip_range=None, api_version=None,
                 cache_control=None, content_disposition=None, content_language=None, content_encoding=None, content_type=None):
        super(BlobSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                          permissions=permissions,
                                                          resource_types='b',
                                                          service_type=_SasConst.SERVICE_TYPE_BLOB,
                                                          https_only=https_only,
                                                          policy=policy,
                                                          ip_range=ip_range,
                                                          api_version=api_version,
                                                          cache_control=cache_control,
                                                          content_disposition=content_disposition,
                                                          content_language=content_language,
                                                          content_encoding=content_encoding,
                                                          content_type=content_type)
        self[_SasConst.BLOB_NAME] = blob_name



class ContainerSasDefinitionParameters(_ContentServiceSasDefinitionParameters):
    def __init__(self, container_name, validity_period, permissions, https_only=False, policy=None, ip_range=None, api_version=None,
                 cache_control=None, content_disposition=None, content_language=None, content_encoding=None, content_type=None):
        super(ContainerSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                               permissions=permissions,
                                                               resource_types='c',
                                                               service_type=_SasConst.SERVICE_TYPE_CONTAINER,
                                                               https_only=https_only,
                                                               policy=policy,
                                                               ip_range=ip_range,
                                                               api_version=api_version,
                                                               cache_control=cache_control,
                                                               content_disposition=content_disposition,
                                                               content_language=content_language,
                                                               content_encoding=content_encoding,
                                                               content_type=content_type)
        self[_SasConst.CONTAINER_NAME] = container_name


class FileSasDefinitionParameters(_ContentServiceSasDefinitionParameters):
    def __init__(self, file_name, validity_period, permissions, https_only=False, policy=None, ip_range=None, api_version=None,
                 cache_control=None, content_disposition=None, content_language=None, content_encoding=None, content_type=None):
        super(FileSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                          permissions=permissions,
                                                          resource_types='f',
                                                          service_type=_SasConst.SERVICE_TYPE_FILE,
                                                          https_only=https_only,
                                                          policy=policy,
                                                          ip_range=ip_range,
                                                          api_version=api_version,
                                                          cache_control=cache_control,
                                                          content_disposition=content_disposition,
                                                          content_language=content_language,
                                                          content_encoding=content_encoding,
                                                          content_type=content_type)
        self[_SasConst.FILE_NAME] = file_name

class ShareSasDefinitionParameters(_ContentServiceSasDefinitionParameters):
    def __init__(self, share_name, validity_period, permissions, https_only=False, policy=None, ip_range=None, api_version=None,
                 cache_control=None, content_disposition=None, content_language=None, content_encoding=None, content_type=None):
        super(ShareSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                           permissions=permissions,
                                                           resource_types='s',
                                                           service_type=_SasConst.SERVICE_TYPE_SHARE,
                                                           https_only=https_only,
                                                           policy=policy,
                                                           ip_range=ip_range,
                                                           api_version=api_version,
                                                           cache_control=cache_control,
                                                           content_disposition=content_disposition,
                                                           content_language=content_language,
                                                           content_encoding=content_encoding,
                                                           content_type=content_type)
        self[_SasConst.SHARE_NAME] = share_name


class TableSasDefinitionParameters(_ServiceSasDefinitionParameters):


class QueueSasDefinitionParameters(_ServiceSasDefinitionParameters):


class AccountSasDefinitionParameters(_SasDefinitionParameters):
    def __init__(self, validity_period, permissions, services, resource_types, https_only=False, policy=None, ip_range=None,
                 api_version=None):
        super(AccountSasDefinitionParameters, self).__init__(validity_period=validity_period,
                                                             permissions=permissions,
                                                             resource_types=resource_types,
                                                             sas_type=_SasConst.SAS_TYPE_ACCOUNT,
                                                             https_only=https_only,
                                                             policy=policy,
                                                             ip_range=ip_range,
                                                             api_version=api_version)

        self[_SasConst.SERVICES] = services


if __name__ == '__main__':
    pass
