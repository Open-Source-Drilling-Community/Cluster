---
title: "How to use the Cluster microservice?"
output: html_document
---

Typical Usage
===
1. Upload a new Cluster using the `Post` web api method.
2. Call the `Get` method with the identifier of the uploaded Cluster as argument. 
The return Json object contains the Cluster description.
3. Optionally send a `Delete` request with the identifier of the Cluster in order to delete the Cluster if you do not 
want to keep the Cluster uploaded on the microservice.


