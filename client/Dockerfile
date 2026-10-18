# The web client image. Built from the client folder:
#   docker build client

# 1. Build the Angular app with Node
FROM node:24-alpine AS build
WORKDIR /client

# No question about usage statistics: nobody can answer it during a Docker build
ENV NG_CLI_ANALYTICS=false

# Package files first: Docker caches "npm ci" and repeats it only when dependencies change
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build

# 2. Serve the built files with nginx: no Node in the final image, only static files and nginx
FROM nginx:stable-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /client/dist/client/browser /usr/share/nginx/html
