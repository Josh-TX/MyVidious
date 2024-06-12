# Developer Notes

The README is targeted for users, whereas this document is targeted towards developers (including my future self)

## About me

I've got 7+ years of experience writing .NET and Angular. I had mostly used MSSQL, but never postgres, so using postgres was something new. I created this just finished developing `my-expense-report`, which used Angular & Angular-Material, so I used that for the admin page, even though that would've been a good candidate for using a new framework.

I initially chose postgres because I wanted something that can run on ARM, but I later realized that because Invidious uses Postgres, I could have a single Postgres container for both Invidious and MyVidious. 

## How to debug locally

First, you need to generate the web files for the admin site. To do so, navigate to the UI directory and `npm install`. Then you can either run `npm run build` for a 1-time build, or you can use `npm run watch` for a build that live-rebuilds on code change. Both commands will output to the `/Server/wwwroot` folder, which is where the server serves static files from.

Next, you need to run some docker containers that the server depends on. Specifically you need 3: Postgres, MeiliSearch, and Invidious. See the bottom file for a docker compose that does this.

Once you have the dependencies running, you can then run the server (MyVidious.csproj). I use visual studio to debug it, but you might also have success with vs vode (intellisense wasn't working for me). Make sure the appsettings.json BaseUrls point to your containers.

If you ever make changes to the Server's Models, you might need to update the typings for the Admin UI. I have an nswag that will auto-generate these typings.

To make changes to the database schema, you first make changes the the Entities class, and then run `dotnet ef DbContext script --context VideoDbContext -o ../Database/sql/videoDbContext.sql`. This ensures that the database and entity framework is in agreement on the schema. Note that you'll likely have to delete the local Postgres volume for these changes to take effect. 

## Docker Compose for local debugging dependencies



```yaml
version: '3.9'

services:

  db: #this server as the database for both 
    image: joshtxdev/myvidious-postgres
    restart: always
    volumes:
      - postgresdata:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    shm_size: 128mb
    environment:
      POSTGRES_PASSWORD: password
# I also use adminer, which provides a web UI for browsing my database data/schema. 
#   adminer:
#     image: adminer
#     restart: always
#     ports:
#       - 8080:8080
  meilisearch:
    image: getmeili/meilisearch
    ports:
      - 7700:7700
    restart: unless-stopped
    volumes:
      - meilisearchdata:/meili_data
    environment:
      MEILI_MASTER_KEY: password2
  invidious: 
    #runs on port 3000
    image: quay.io/invidious/invidious:latest-arm64
    restart: unless-stopped
    ports:
      - 3000:3000
    environment:
      INVIDIOUS_CONFIG: |
        db:
          dbname: invidious
          user: postgres
          password: password
          host: sql
          port: 5432
        check_tables: true
        external_port: 3000
    depends_on:
      - db
volumes:
  postgresdata:
  meilisearchdata:
```