### Overview

This project should help users to access Kinde api with oauth authentication. It contains 3 pre-built OAuth flows: client credentials, authorization code and authorization code with PKCE code verifier. After build it produces nuget package, which should be published to nuget repository.

### Build

VS automaticaly recreates access client using yaml metadata on build. Right now it references to static file, but it can be re-targeted to some web published file to get latest one for each build. in this case nuget will be built with latest api version. Nuget package will be built automaticaly too. But publishing to nuget.org requires additional setup. 

### Usage

##### Don't use constructor without <code>IIdentityProviderConfiguration</code> parameter. This will throw exceptions.

Identity provider configuration contains these parameters:
- Domain. Base domain used for authorization. must be subdomain of kinde.com. F.E. testauth.kinde.com
- ReplyUrl. Unused for client credentials, but used as callback url for other flows. May be null for client credentials
- LogoutUrl. Url for redirection after logout.

Additional requirements (based on flow)
- ClientID, required.
- ClientSecret, required.
- Scope, required.
- State, optional. Can be set to null, then will be autogenerated. <b> Note, that state parameter must not be constant. It must be random for each call.</b>
- Code verifier generic parameter, required. Use inbuilt SHA256CodeVerifier, or create other one if needed. 

SDK supports configuration from appsetings.json. Also, you can write your own implementation of ```IAuthorizationConfigurationProvider``` and ```IIdentityProviderConfigurationProvider```. 
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
Also, you should register configuration providers using .net DI:
```csharp
builder.Services.AddTransient<IAuthorizationConfigurationProvider, DefaultAuthorizationConfigurationProvider>();
builder.Services.AddTransient<IApplicationConfigurationProvider, DefaultApplicationConfigurationProvider>();
```
PKCES256Configutation is most complicated configuration and contains all necessary properties. Configuration for Authentication code is same and for Client credentials State is not applicable. But it is not mandatory to remove it.
All availiable types are:
1. Kinde.Api.Models.Configuration.PKCES256Configutation
2. Kinde.Api.Models.Configuration.AuthorizationCodeConfiguration
3. Kinde.Api.Models.Configuration.ClientCredentialsConfiguration

Besides configuration, all code approach is quite similar. Only difference is if authorization flow requires user redirection or not.
For Client configuration ```Authorize()``` call is enough for authoriztion.
For others (PKCE and Authorization code) you should handle redirection to Kinde (as IdP) and handle callback to end authorization.


#### 1. Login with no redirection, using Client credentials flow
For not web application only client credentials flow can be used. Because other flows requires user interaction via browser. 
Example: <br>
```csharp
var client = new Kinde.KindeClient(
    new IdentityProviderConfiguration("https://testauth.kinde.com", "https://test.domain.com/callback", "https://test.domain.com/logout"), 
    new KindeHttpClient());
await client.Authorize(new ClientCredentialsConfiguration("clientId_here","openid offline any_other_scope", "client secret here"));
// Api call
//   vvv
var myOrganization  = client.CreateOrganizationAsync("My new best organization");
```

After this you can call any api methods on this instance.

#### 2. Login with redirection, using PKCE256 flow or Authorization code

For web applications api clients should be connected to user session. To keep them sync use <code>KindeClientFactory</code>. I t has thread safe dictionary to save client instances. It is highly recommended to use Session Id or something like this as a key for instance.

Example:<br>
```csharp
   public async Task<IActionResult> Login()
   {
        // We need some artificial id to correlate user session to client instance
        //NOTE: Session.Id will be always random, we need to add something to session to make it persistent. 
        string correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("KindeCorrelationId", correlationId);
        }
        // Get client's instance...
        var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
        // ...and authorize it
        await client.Authorize(_authConfigurationProvider.Get());
        // if auth flow is not ClientCredentials flow, we need to redirect user to another page
        if (client.AuthotizationState == Api.Enums.AuthotizationStates.UserActionsNeeded)
        {
            // redirect user to login page
            return Redirect(await client.GetRedirectionUrl(correlationId));
        }
        return RedirectToAction("Index");
   }
```

This code won't authenticate user complletely. We should wait for data on callback endpoint and execute this: <br>
```csharp
        public IActionResult Callback(string code, string state)
        {
            Kinde.KindeClient.OnCodeRecieved(code, state);
            string correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            var client = KindeClientFactory.Instance.Get(correlationId); //already authorized instance
            // Api call
            //   vvv
            var myOrganization  = client.CreateOrganizationAsync("My new best organization");
            return RedirectToAction("Index");
        }

```
#### Register user
User registration is same as authorization. With one tiny difference:
```csharp
     public async Task<IActionResult> SignUp()
        {
            string correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("KindeCorrelationId", correlationId);
            }
            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            await client.Authorize(_authConfigurationProvider.Get(), true); //<--- Pass true to register user
            if (client.AuthotizationState == Api.Enums.AuthotizationStates.UserActionsNeeded)
            {
                return Redirect(await client.GetRedirectionUrl(correlationId));
            }
            return RedirectToAction("Index");
        }
```

#### Logout

Logout has two steps: local cache cleanup and token revocation on Kinde side. So, client.Logout() method sholud be called. This method returns redirect url for token revocation. User should be redirected to it.

Logout example:
```csharp
  public async Task<IActionResult> Logout()
        {
            string correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
          
            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
             var url = await client.Logout();
            
            return Redirect(url);
        }
```
#### Token renewal
Token will renew automatically in background. If you want to do it manually, call Renew method.
Example:
```csharp
public async Task<IActionResult> Renew()
        {
            string correlationId = HttpContext.Session?.GetString("KindeCorrelationId");

            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            await client.Renew();

            return RedirectToAction("Index");
        }
```

## Usage
### Calling API
```csharp
 // Don't forget to add "using Kinde;", all data objects models located in this namespace 
 var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
 Users users = (Users)await client.GetUsersAsync(sort: Sort.Name_asc, page_size: 20, user_id: null, next_token: "next", cancellationToken: CancellationToken.None );
 foreach(User user in users){
    Console.WriteLine(user.Full_name + " is awesome!");
 }
```

### User profile

Note, that some of claims and properties will be unavaliable if scope 'profile' wasn't used wi]hile authorizing. In this case null will be returned.
```csharp
                var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
                 user = client.User;
                var id = user.Id; // get user id
                var gName = user.GivenName; // get given name
                var fName = user.FamilyName;// get family name
                var email = user.Email;// get email
                var claim = user.GetClaim("sub"); // get claim 
                var org = user.GetOrganisation(); // get primary organisation
                var orgs = user.GetOrganisations(); // get all avaliable organisations for user
```
More usage examples can be found in Kinde.DemoMvc project.

