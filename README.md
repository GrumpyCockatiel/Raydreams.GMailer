# Raydreams.GMailer

Examples using GMail API with .NET 6 starting with Bulk Forwarding.

## Tech Stack

C#/.NET 6

## Use Case

As a GMail user I want to bulk forward several emails from one of my GMail accounts to any other email address, preserving all the attachments and original sender. I don't want to use a 3rd party tool which might 'phone home' my emails because I'm not a gullible idiot.

## GMail API Key

You will need a GMail API Client ID and Secret which you can create on Google Cloud.

I'll add intructions on how to do this later but it's pretty much like creating any other API Key.

## App Configuration

You will need to add an `appsettings.json` file to your project with the added section **AppConfig** in the JSON root. You can hard code these values in the AppConfig Class to get started.

```
{
"AppConfig": {
    "Environment": "DEV",
    "UserID": "My.Gmail.Account@gmail.com",
    "ForwardToName": "Bubba Jack",
    "ForwardToAddress": "bubbajack@outlook.com",
    "ClientID": "<My GMail API Client ID>",
    "ClientSecret": "<My GMail API Secret>",
    "Top": 10
},
"ConnectionStrings": {
    "DefaultConnection": "DataSource=app.db"
},
"Logging": {
    "LogLevel": {
        "Default": "Warning"
    }
},
"AllowedHosts": "*"
}
```