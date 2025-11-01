## Project Overview

This project is a general-purpose web crawler designed to extract and
save website content, capable of being used with any website structure.

### Key Features

-   **Generic architecture** suitable for crawling any website.
-   **Breadth-first recursive crawling** to ensure complete coverage of
    target content.
-   **Local disk storage** for saving extracted data, ensuring offline
    access to downloaded files.
-   **Preserved directory hierarchy** using nested parent-child folders.
    Folder names are generated from page URLs for easy traceability.
-   **Targeted element extraction** based on HTML section IDs to ensure
    only required text and file resources are collected.
-   **Duplicate prevention** using hash validation for URLs and
    downloaded files.
-   **Built-in logging and reporting** at start, during processing, and
    upon completion.

------------------------------------------------------------------------

## Configuration

-   Supports extraction of **Text / PDF / Word / Excel / PowerPoint**
    files by default.
-   Can be configured to extract **image, video, and audio** files
    (disabled by default). Configuration located in `CrawlerConfig.cs`.
-   Option to **restrict crawling to the main domain** or allow
    cross-domain crawling.
-   **Adjustable crawl depth (MaxDepth)** --- you can configure how deep
    the crawler explores pages.\
    If not provided in input, it defaults to **10 levels**.
-   **Adjustable delay interval** between requests.
-   Flexible **CSS selector configuration**, with ability to add or
    remove selectors as needed.

------------------------------------------------------------------------

## Input Parameters

  ---------------------------------------------------------------------------------
  Parameter               Description                  
  ----------------------- ---------------------------- ----------------------------
  `Url`                   Target website address for   
                          crawling. If not provided,
                          default to `https://www.example.com/`.                     

  `SectionId`             The ID of the element you want.
                          If not entered, if TargetElementId is disabled,
                          it will crawl the entire page,
                          and if enabled, it will crawl the same predefined elements.             

  `MaxDepth`              Maximum recursion depth for  
                          crawling. Can be customized. 
                          If not set, defaults to      
                          **10**                       
  ---------------------------------------------------------------------------------

------------------------------------------------------------------------

## Notes

-   Folder structure and URL-based naming ensure high traceability and
    easy navigation of saved content.
-   The system is optimized to avoid re-visiting URLs and re-downloading
    identical files.
