# Kinde Starter Kit - .NET

## Register an account on Kinde

To get started set up an account on [Kinde](https://app.kinde.com/register).

## Setup your local environment

Clone this repo and Open your solution or project in Visual Studio. Right-click on the solution or project in the Solution Explorer. Choose "Restore NuGet Packages". This will install all the listed packages


The starter kit supports configuration using values from defined in the `appsetings.json` file.

Set the following variables with the details from the Kinde `App Keys` page

> Authority - Your Kinde domain
>
> ClientId - Your client Id
>
> ClientSecret - Your client secret

e.g:
```json
  "Authentication": {
    "Schemes": {
      "OpenIdConnect": {
        "Authority": "<your_kinde_domain>",
        "ClientId": "<your_client_id>",
        "ClientSecret": "<your_client_secret>",
        "MapInboundClaims": false,
        "ResponseType": "code",
        "SignedOutRedirectUri": "/"
      }
    }
  }
```

## Set your Callback and Logout URLs

Your user will be redirected to Kinde to authenticate. After they have logged in or registered they will be redirected back to your .NET application.

You need to specify in Kinde which url you would like your user to be redirected to in order to authenticate your app.

On the App Keys page set ` Allowed callback URLs` to `https://localhost:7165/signin-oidc`

> Important! This is required for your users to successfully log in to your app.

You will also need to set the url they will be redirected to upon logout. Set the `Allowed logout redirect URLs` to `https://localhost:7165/signout-callback-oidc`.

## Start the app

Start your server through Visual Studio and navigate to `http://localhost:7165`.

Click on `Sign up` and register your first user for your business!

## View users in Kinde

If you navigate to the "Users" page within Kinde you will see your newly registered user there. 🚀

## Note

When setting up this project on a Windows machine with Visual Studio Code, SSL certificates will be automatically provisioned, please ensure then that you are using https protocol when specifying your redirect and logout redirect URLs.