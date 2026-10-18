# Client

The Angular 22 web client of CommentFlow. How to run it, build it and test it is described in the
[main README](../README.md#local-development).

```bash
npm ci
npx ng serve                 # http://localhost:4200, /api, /uploads and /hubs go to the API (proxy.conf.json)
npx ng build                 # production build in dist/client/browser
npx ng test --watch=false    # unit tests (Vitest)
```

In Docker the built app is served by nginx: see [Dockerfile](Dockerfile) and [nginx.conf](nginx.conf).
