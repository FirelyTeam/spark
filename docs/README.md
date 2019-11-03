# Documentation

This is the source for the Spark documentation pages. Documentation pages are built by Gatzby, and presented by the Docz theme. 


## Add more documentation pages
To add pages, just add files in the `docs/src/pages` folder. 

Note the formatting of files: 

```
---
name: Test 1
route: /test1
menu: Group name in menu
---
# What is this page about?
## Even more documentation
Type here the most beautiful lines describing Spark. 
``` 

## Deploy pages
To deploy to Github pages, run `npm run deply`. 