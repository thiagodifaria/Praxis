FROM node:20-alpine AS build

WORKDIR /app/client-web

COPY client-web/package.json ./
COPY client-web/package-lock.json ./

RUN npm ci

COPY client-web/ ./

RUN npm run build

FROM nginx:1.27-alpine

COPY infra/server/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/client-web/dist/client-web/browser /usr/share/nginx/html

EXPOSE 80
