# ---------- Build stage ----------
FROM node:20-alpine AS build
WORKDIR /app

# Build args/env for Vite
ARG VITE_API_URL=/api
ARG VITE_API_BASE=/api
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_API_BASE=${VITE_API_BASE}

# Install deps
COPY package*.json ./
RUN npm ci || npm i

# Copy source & build
COPY . .
RUN npm run build

# ---------- Runtime stage ----------
FROM nginx:alpine
# Nginx with SPA + /api proxy to backend:8080 (docker network name)
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://127.0.0.1/ || exit 1
CMD ["nginx", "-g", "daemon off;"]
