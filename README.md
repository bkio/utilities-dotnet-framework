Copyright 2022- Burak Kara, All rights reserved.
 
 # UtilitiesDotnetFramework

This is a microservice framework for .NET Core 3 and .NET Framework 4.7+ that abstracts many cloud features such as;

 - Cloud specific managed no-sql database access for
	 - Amazon: Dynamodb
	 - Google Cloud: Datastore
	 - Azure (or generic): Mongodb
 - Cloud storage access for
	 - Amazon: S3
	 - Google: GCS
	 - Azure: Storage Blobs
 - Cloud logging service access for
	 - Amazon: Cloud Watch
	 - Google: Stackdriver
	 - Azure: Azure Telemetry
 - Pub/Sub service access for
	 - Amazon: SQS
	 - Google: Google Pub/Sub
	 - Azure: Eventgrid
	 - Generic: Redis Pub/Sub
 - In-memory service access for:
	 - Generic: Redis
 - Cloud e-mail service access for
	 - Sendgrid
 - Tracing service access for
	 - Zipkin
 - Virtual machine management:
	 - Google: Compute Engine
	 - Azure: Virtual Machines
	 - AWS: TODO

The framework does not use any http library such as ASP.NET or OWIN, it uses the basic HttpListener by abstracting wildcard path parsing to make the usage even easier and increase the compatibility with different platforms.
