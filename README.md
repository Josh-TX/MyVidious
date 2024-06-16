# MyVidious

An Invidious-Compatible API wherin you can configure a whitelist of Youtube Channels & Playlists, such that search results and the suggestion algorithm only includes the whitelisted content. Apps that work with the invidious API will also work with MyVidious (FreeTube on desktop, Yattee on iOS, Clipious on Android).

I will soon make a video explaining it

## What's Invidious? 
https://messages.google.com/web/conversations
[Invidious](https://github.com/iv-org/invidious) is a an alternative website & API for interacting with Youtube. They don't have an official website, but they have a list of [public instances](https://docs.invidious.io/instances/) that volunteers operate. It's primary purpose is privacy and bypassing ads. Because it exposes an API, there are many apps that to work with Invidious (FreeTube on desktop, Yattee on iOS, Clipious on Android, [see full list](https://docs.invidious.io/applications/)). 

I have no affiliation with Invidious, however, MyVidious is designed to work in the middle of the Invidious ecosystem. Instead of the Invidious Apps connecting to the Invidious API, they can connect to the MyVidious API, since it's compatible. MyVidious in turn will propogate some requests to Invidious the Invidious API, though some requests may be handled entirely by MyVidious.

## How do I use it?

Unfortunately, there won't be an official hosted version of MyVidious. Instead, you or a close friend will have to privately host it yourself via Docker. See more details in the installation section below.

I do have a demo version publically available at [http://myvidious.duckdns.org](http://myvidious.duckdns.org). It has bad performance, registration is closed, and there's only 1 algorithm with a few channels. It is after all just a demo version. You can use an app like Clipious on Android or Yatte on iOS, and in the app's settings set the server url to `http://myvidious.duckdns.org/josh/main`. By far the biggest downside of the demo version is that registration is closed, meaning you can't create you're own custom algorithm like you could on a private version. 

## Installation

The server and all its dependencies should be run via Docker. The following shows the simplest-possible docker-compose file.

```yaml
version: '3.9'

services:
  myvidious:
    image: joshtxdev/myvidious:latest
    restart: unless-stopped
    ports: 
     - 4000:8080
    environment:
      CONNECTIONSTRING: Server=db;Port=5432;Database=myvidious;User Id=postgres;Password=PostgresPassword;
      INTERNALINVIDIOUSURL: http://invidious:3000
      MEILISEARCHURL: http://meilisearch:7700
      MEILISEARCHKEY: MeilisearchPassword
    depends_on:
      - db
      - meilisearch
  db:
    #internally runs on port 5432
    image: joshtxdev/myvidious-postgres:latest
    restart: unless-stopped
    volumes:
      - postgresdata:/var/lib/postgresql/data
    environment:
      POSTGRES_PASSWORD: PostgresPassword
  meilisearch:
    #internally runs on port 7700
    image: getmeili/meilisearch
    restart: unless-stopped
    volumes:
      - meilisearchdata:/meili_data
    environment:
      MEILI_MASTER_KEY: MeilisearchPassword
  invidious: 
    #internally runs on port 3000
    #if running on arm64, use the tag :latest-arm64
    image: quay.io/invidious/invidious:latest
    restart: unless-stopped
    environment:
      INVIDIOUS_CONFIG: |
        db:
          dbname: invidious
          user: postgres
          password: PostgresPassword
          host: db
          port: 5432
        check_tables: true
        hmac_key: "RandomString"
    depends_on:
      - db
volumes:
  postgresdata:
  meilisearchdata:
```

see advanced configuration for more options. I recommend adding an EXTERNALINVIDIOUSURL environmental variable to Myvidious so that images can go directly to Invidious rather than being proxied through MyVidious. 

## Usage

When you go to the root url of MyVidious, you'll be greeted by a screen explaining that there are no algorithms, and you need to go to the /admin page to create one. The admin page should prompt you to create user, and if you're the first user, you become the one and only "head admin". The app starts out as open-invite (anyone can create a new user), but the head admin can change this from the "invite codes" page. Managing invite codes is the only privledge exclusive to the head admin. 

Once logged in, you can create a new algorithm. To add channels to your algorithm, type into the "Search Channels" input, and a dropdown will appear with search results. Clicking a search result will add it to your algorithm. Unfortunately, the invidious API doesn't provide accurate numbers for the Video Count of the channel. After clicking the blue "Save Changes" button, the algorithm will be created, and in the background MyVidious will begin scrapping data for the new channels. After saving, you can refresh the page repeatedly, and you should notice the Video Count being updated every second. The page has many gray info circles that you can hover over, and this provides additional information about the field. 

The created algorithm will cause there to be an invidious-compatible API located at the path `myvidious-baseUrl/username/algorithmName`. The reason for this format is that the invidious apps (Clipious, Yattee), don't have any concept of an "Algorithm". The only thing we have control over is the server url, so by putting the selected algorithm in the url path, we have a means of selected which algorithm the app should use. There's a similar restriction with authentication. The apps don't know how to log in to MyVidious, and so the algorithms have to be public. There is an option to unlist your algorithm, so it won't show up on the MyVidious root page, but it's still public.

The algorithm itself will mostly suggest random videos from the channels, in accordance with the percentages specified by the algorithm. Unlike youtube, there's no recency-bias or popularity-bias: All videos on a channel/playlist have an equal likelyhood of being suggested. The one exception is Invidious' Popular Page, which will only show the 3 most recent videos from each channel/playlist on the algorithm. 

The algorithm works by computing & caching a list of 500 videos (possibly containing duplicates), and there's an IpAddress-scoped iterator starting at position 0. The iterator advances as suggestions are returned, and the iterator will loop back from 500 to 0 if needed. The cache & is reset after 30 minutes. Invidious' "Trending" page will utilize this same cache, but in reverse (so the starting at the 500th video and iterating backwards). Unlike the forward iterator, there's no backward iterator stored, so the trending page will show the same videos for 30-minute intervals. 

## Advanced Configuration

work in progress







