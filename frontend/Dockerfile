FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS final
# The esbuild-based Angular application builder emits the servable output under a `browser/`
# subfolder (dist/frontend/browser/index.html), not directly in dist/frontend — copying the parent
# folder left nginx's html root without an index.html, silently falling back to its default page.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
