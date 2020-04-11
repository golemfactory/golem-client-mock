# golem-client-mock
Mockup of Golem Client with all the standardized APIs.

## Building the project

### Prerequisites
- .NET Core SDK 2.2
- NPM (Node.js package manager) - required for React frontend application

### Windows

Install the prerequisites, then:

Get project sources:
```
git clone https://github.com/stranger80/golem-client-mock.git
```

Get NPM dependencies for client app:
```
cd golem-client-mock/GolemClientMockAPI/ClientApp
npm install
cd ../..
```

Build the project:
```
dotnet publish
```

### Ubuntu

**Setup prerequisites:**

(As per [https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current], but note .NET Core 2.2. is required rather than the latest version) :

```
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
```

Get .NET Core 2.2. SDK

```
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.2
```

Get NPM:

```
sudo apt install npm
```

and then follow section for [Windows](#Windows)

### macOS

**Setup prerequisites:**
- Get .NET Core 2.2. SDK: https://dotnet.microsoft.com/download/dotnet-core/2.2
- Get NPM using brew: https://treehouse.github.io/installation-guides/mac/node-mac.html
```
brew update
brew install node
```
and then follow section for [Windows](#Windows)

## Running the project in Development mode
 
In command prompt, in golem-client-mock folder, run:
```
dotnet run -p GolemClientMockAPI
```
...or (Linux):
```
./start_api.sh
```

...and observe the messages from the API startup.

Open the browser to view the Control Dashboard:
```
http://localhost:5001/
```

...or view the Swagger UI:
```
http://localhost:5001/swagger
```

## Running the project in Docker

### Building Docker image

Run the `docker_build.bat` script.

### Running Docker image

Run the `docker_run.bat` script.
