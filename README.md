Copyright 2022- Burak Kara, All rights reserved.
 
 # UtilitiesDotnetFramework

This is a microservice framework for .NET Core 3 and .NET Framework 4.7+ that abstracts many cloud features such as;

 - Cloud specific managed no-sql database access for
	 - Amazon: Dynamodb
	 - Google Cloud: Datastore
 - Cloud storage access for
	 - Amazon: S3
	 - Google: GCS
 - Cloud logging service access for
	 - Amazon: Cloud Watch
	 - Google: Stackdriver
 - Pub/Sub service access for
	 - Amazon: SQS
	 - Google: Google Pub/Sub
	 - Generic: Redis Pub/Sub
 - In-memory service access for:
	 - Generic: Redis
 - Cloud e-mail service access for
	 - Sendgrid
 - Tracing service access for
	 - Zipkin

The framework does not use any http library such as ASP.NET or OWIN, it uses the basic HttpListener by abstracting wildcard path parsing to make the usage even easier and increase the compatibility with different platforms.