FROM registry.access.redhat.com/ubi8/ubi-minimal

# Update packages and install nginx and gettext (which provides envsubst)
RUN microdnf update -y && \
    microdnf install -y nginx gettext && \
    microdnf clean all

# Create a non-root user and group (UID/GID 1000)
RUN adduser -u 1000 -S nginx

# Change ownership of necessary directories so the non-root user can write
RUN mkdir -p /var/log/nginx /var/run && \
    chown -R nginx:nginx /var/log/nginx /var/run /etc/nginx

# Copy the templated nginx configuration file and the entrypoint script
COPY nginx.conf.template /etc/nginx/nginx.conf.template
COPY entrypoint.sh /entrypoint.sh

# Make the entrypoint script executable and change its ownership
RUN chmod +x /entrypoint.sh && chown nginx:nginx /entrypoint.sh

# Expose HTTP and HTTPS ports
EXPOSE 80 443

# Switch to the non-root user
USER nginx

# Set the entrypoint
ENTRYPOINT ["/entrypoint.sh"]