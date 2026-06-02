/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Proxy /api/* to the ASP.NET backend so the browser stays same-origin and
  // we don't need to add CORS to the backend. Configure the target via the
  // BACKEND_URL environment variable in .env.local (defaults to localhost:5094).
  async rewrites() {
    const target =
      process.env.BACKEND_URL?.replace(/\/+$/, "") ?? "http://localhost:5094";
    return [
      {
        source: "/api/:path*",
        destination: `${target}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
