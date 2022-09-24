# Raydreams.GMailer

Examples using GMail API with .NET 6 starting with Bulk Forwardeding

## Tech Stack

.NET 6 

## HOW TO

You have to add an appsettings.json file to your project with the added section in the JSON root

```
"AppConfig": {
    "Environment": "DEV",
    "UserID": "myGmail@gmail.com",
    "ForwardTo": "EmailToForwardTo@outlook.com",
    "ClientID": "<My GMail API Client ID>",
    "ClientSecret": "<My GMail API Secret>",
    "Top":10
},
``` 