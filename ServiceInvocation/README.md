# Dapr Service-to-Service Invocation Demo

This demo shows how to use Dapr and console application (httpclient) to make invoke REST API between services.

## Run Demo with Dapr

Run the following command to start the demo:

Windows:
1. Start service service_a:
```powershell
./dapr-service_a.ps1
```

2. Start client to invoke service_a:
```powershell
./dapr-client_service_a.ps1
```

## Demo Output

Show the different ways to invoke a service:
- Using standard HTTP Client
- Using Dapr HTTP Client
- Using Dapr Client

Make sure to define the different ways in which you can send datas in the different methods.

## Run Demo with console application

Run the following command to start the demo:

Windows:
1. Start service service_a running on port 9080:
```powershell
./console_service_a-9080
```

2. Start client to invoke service_a:
```powershell
./console_client_service_a.ps1
```

## Demo Output
