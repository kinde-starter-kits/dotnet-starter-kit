### Usage

The Kinde .NET Starter Kit allows developers to quickly and securely integrate launch an existing .NET application that is already setup with the [Kinde SDK](https://github.com/kinde-oss/kinde-dotnet-sdk). 

Configured the starter kit by providing your Kinde configuration values to the `IIdentityProviderConfiguration` class
Identity provider configuration contains these parameters:
- Domain. Base domain used for authorization. must be subdomain of kinde.com. F.E. testauth.kinde.com
- ReplyUrl. Unused for client credentials, but used as callback url for other flows. May be null for client credentials
- LogoutUrl. Url for redirection after logout.

**Note:** Please don't use the constructor without the <code>IIdentityProviderConfiguration</code> parameter, otherwise the constructor will throw exceptions. 

The starter kit supports configuration using values from defined in the `appsetings.json` file.

Configuration example:
```json
"ApplicationConfiguration": {
    "Domain": "https://testauth.kinde.com",
    "ReplyUrl": "https://localhost:7165/home/callback",
    "LogoutUrl":  "https://localhost:7165/home"
  },
  "DefaultAuthorizationConfiguration": {
    "ConfigurationType": "Kinde.Api.Models.Configuration.PKCES256Configutation",
    "Configuration": {
      "State": null,
      "ClientId": "reg@live",
      "Scope": "openid offline",
      "GrantType": "code id_token token",
      "ClientSecret": "<my secret>"
    }
  },
```


