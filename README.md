# External ID Admin Portal (.NET 8)

A custom admin portal built with .NET 8 for managing identities via Microsoft Entra External ID.

## 🚀 Prerequisites

Before running this project, ensure you have the following installed and configured:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- A Microsoft Entra ID tenant
- A Microsoft Entra External ID tenant

## 🔧 Setup Instructions

### 1. Install .NET 8

Download and install the .NET 8 SDK from the official [.NET website](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### 2. Create an App Registration in Entra ID

1. Go to [Microsoft Entra Admin Center](https://entra.microsoft.com).
2. Navigate to **Entra ID** > **App registrations** > **New registration**.
3. Set the following:
   - **Name**: `External ID Admin Portal`
   - **Redirect URI**: `https://localhost:7059/signin-oidc` (or your deployed URI)
4. Go to **Authentication** > **Settings** and enable ID tokens.
5. Note down the following values:
   - **Client ID**
   - **Directory (Tenant) ID**

### 3. Create an App Registration in Entra External ID

1. Go to [Microsoft Entra Admin Center](https://entra.microsoft.com).
2. Navigate to **External ID** > **App registrations** > **New registration**.
3. Set the following:
   - **Name**: `External ID Admin Portal`
4. After registration, go to **API permissions** and add:
   - `User.ReadWrite.All` (Application)
5. Go to **Certificates & secrets** and generate a **Client Secret**.
6. Note down the following values:
   - **Client ID**
   - **Client Secret**
   - **Directory (Tenant) ID**

### 4. Populate appsettings.json

Within the root level of src, open the appsettings.json and do the following:
   - Populate AzureAd section with the values from **Create an App Registration in Entra ID**
   - Populate GraphApi section with the values from **Create an App Registration in Entra External ID**
   - Populdate UserAttributeMappings with the user attributes you want to be able to read/write in addition to default attributes
   - Populate SearchableAttributes with the user attributes you want to be able to search by
   - Populate ExtensionAttributeGuid with the Client ID (WITHOUT HYPHENS) from the **b2c-extensions-app** application registration


