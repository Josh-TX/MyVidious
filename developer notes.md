# Developer Notes

The README is targeted for users, whereas this document is targeted towards developers (including my future self)

## About me

I've got 7+ years of experience writing .NET and Angular. I had mostly used MSSQL, but never postgres, so using postgres was something new. I created this just finished developing `my-expense-report`, which used Angular & Angular-Material, so I used that for the admin page, even though that would've been a good candidate for using a new framework.

I initially chose postgres because I wanted something that can run on ARM, but I later realized that because Invidious uses Postgres, I could have a single Postgres container for both Invidious and MyVidious. 

## How to debug locally

First, you need to generate the web files for the admin site. To do so, navigate to the UI directory and `npm install`. Then you can either run `npm run build` for a 1-time build, or you can use `npm run watch` for a build that live-rebuilds on code change. Both commands will output to the `/Server/wwwroot` folder, which is where the server serves static files from.

Next, you need to run some docker containers that the server depends on. Specifically you need 3: Postgres, MeiliSearch, and Invidious. I have a docker-compose file in the `/Server` folder which runs the needed containers. 

Once you have the dependencies running, you can then run the server (MyVidious.csproj). I use visual studio to debug it, but you might also have success with VScode (intellisense wasn't working for me). Make sure the appsettings.Development.json BaseUrls points to your containers. If you use `/Server/docker-compose`, the current appsettings.Development.json settings should already work. 

If you ever make changes to the Server's Models, you might need to update the typings for the Admin UI. I have an nswag that will auto-generate these typings.

To make changes to the database schema, you first make changes the the Entities class, and then run `dotnet ef DbContext script --context VideoDbContext -o ../Database/sql/videoDbContext.sql`. This should write a file to `Database/sql/videoDbContext.sql`, which contains sql commands to create the schema needed for the `VideoDbContext`. Unfortunately, this assumes you're starting with an empty database. It probably won't work for modifying an existing schema. When I make changes, I usually delete the existing database and then apply the new schema. 