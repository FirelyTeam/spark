---
name: Contribute to Spark
route: /contribute
---

# Contributions
You are welcome to contribute to this project. The Spark server is used in several commercial and open source projects. Therefore we have a high quality standard and we carefully review submissions. 

When you want to contribute changes:
- Contact us before start working on a major change.
- Fork and send us a pull request

### Pull requests
When you send us a pull request
- Make sure it builds
- Make sure it's tested 
- The pull request should cover only one change
- Accept that we might reject it because it conflicts with our strategy.

We do appreciate suggestions, but the Spark FHIR server code is used by us for commercial projects, so we will most probably reject substantial changes unless you coordinate them with us first. 

### GIT branching strategy 
Our strategy for git branching:
- R4:
    - Current stable: **r4/master**
    - Current nightly build: **r4/develop**
- STU3:
    - Current stable: **stu3/master**
    - Current nightly build: **stu3/develop**
- DSTU2:
    - Current stable: **master**
    - Current nightly build: **develop**
- Feature branches: **feature**/topic
- bugfixes: **bugfix/**issue

Based on:
- [NVIE](http://nvie.com/posts/a-successful-git-branching-model/)
- Or see: [Git workflow](https://www.atlassian.com/git/workflows#!workflow-gitflow)