# ugfzf - A programmer's guide

Please read the user's guide first, where it is explained what the program does
and how to use it. This documents presumes that knowledge in its reader.

## About

ugfzf consists of two parts - scraping library and user interface. These are
separated into their respective directories in the repo.

Some third party libraries were also used. More on them later.

## UGScraper

A library for scraping content and search results off of ultimate-guitar.com
(I will call the site UG from now on).

### `BaseScraper`

The actual scraping is done in the `BaseScraper` class. The `ScrapeUrl` method
retrieves the document, deserializes it and returns the data as a `JsonNode`.

The way UG works is that in the retrieved HTML document there is a `<div>`
which stores all the dynamic data as HTML encoded JSON in an attribute. Not
very pretty, but it certainly made things much easier than I expected them to
be when starting the project.

So we load the document, find the element, get the value of the `data-content`
attribute, decode it, deserialize it and that's the basics done.

`BaseScraper` is an `abstract` class. Besides the `ScrapeUrl` method it also
defines some common interface for the two `instatiable` scraper classes.

```c#
void LoadData(string query)
```

The idea here is, that I want the objects to be reusable and also doing network
I/O through the constructor just feels wrong.

Different scraper types would use this method in different ways, but always
with the result, that it loads data for further manipulation based on the query
(can be a search term or an url or whatever makes sense theoretically).

```c#
uint GetNextItemUid();
```

We want to provide the user with the ability to easily distinguish between
scraper records, but eventhough UG has its own system of IDs we cannot
guarantee their presence in the scraped data (not just theoretically - the
JSONs UG provides are a huge mess apparently). Therefore we add our own uid.

### `PageScraper`

Object encapsulating state and functionality needed for scraping contentful UG
web documents, meaning documents containing tabs, chords etc.

Basically all it does is, that it loads a single URL, loads its data and
deserializes it into a `ScraperRecord` type.

### `SearchScraper`

Object encapsulating state and functionality needed for scraping search queries
off of UG.

It accepts a search query and returns the best it can find on UG. This is done
simply through encoding the query and inserting it in a template URL which
yields one page of the search results.

We need to download a single page first, which among its content also contains
information about the number of available search page results. We then simply
repeat the scraping process for the rest of the search pages.

About url parameters accepted by UG:

```
search_type - "title", "band" and possibly other stuff (eg forum posts)
value       - URL/percent-encoded query
page        - used when results don't fit on a single page; starts at 1
type        - filter by content type; see enum contentType
            - no "type" parameter means "all"
            - multiple allowed types may be specified using multiple parameters
              of the form type[n]=value, where n starts at 0
```

Same as `PageScraper` it then deserializes the data to the `ScraperRecord` type
and hands them over to the user. The `Content` field will always be empty though.

### `ScraperRecord` and `DeserializationRecord`

`ScraperRecord` is a readonly type meant for the user, who has no way of even
instatiating it. It contains curated and, where appropriate transformed, scrape data.

`DeserializationRecord` is just a boilerplate helper type, which allows the
implementation to use reflection for easy deserialization.

## The client

The `ugfzf` directory contains the code for the actual console application.

Not much is going on in `main.cs`. We only define and parse cmdline options
here, which is a breeze thanks to the help of a the `CommandLine` library.

The interesting stuff is happening in `cli.cs`.

The `Cli` class represents the user interface, its state and logic.

### `Run()`

After the class is initialized this is the program's entrypoint.

The app can run in two modes:

1) interactive - enter a search query then select one of the results
2) url-mode - scrapes the content of URLs provided as commandline args

Based on whether the `--url` option was specified `Run()` directs execution.

### `FetchAndPrint()`

This method recieves a list of urls, and consecutively fetches their content
and displays it to the user. It gets called directly if running in
non-interactive mode and indirectly if otherwise.

### `SearchAndPrint()`

Executed when running in interactive mode.

First it loads search results for the user given query and filters them, to
only allow types of content, which the user requested and which aren't broken -
that is missing the URL to the actual contentful web document.

It then recieves user's choices through `GetFzfUserInput()` and hands the
relevant URLs over to `FetchAndPrint()`.

### `GetFzfUserInput()`

This is where the fun begins :)

We run an external program - `fzf` - to get the user selection from the
multitude of search results.

The way `fzf` works is, that it takes lines of text on stdin and prints the
ones selected by user on stdout. May sound trivial, but try running it yourself
and you'll understand, that it's actually very nice.

Each line is preceded with the `ScraperRecord` uid, which fzf allows us to not
display to the user and not include in the search, and followed by formatted
entry containing things like song name, artist and type of content.

We read fzf's output and parse out all the UIDs from it which we then return.

If fzf encounters an error it makes no sense for us to continue execution, so
we kill the program with an error message and an exit code.

## Libraries

Besides the standard library, two external libraries were used:

* HTMLAgilityPack - for downloading and parsing HTML documents
* CommandLine - for parsing command line arguments

These are both MIT licensed, so there are no issues with me opting for GPLv3
license for this project.
