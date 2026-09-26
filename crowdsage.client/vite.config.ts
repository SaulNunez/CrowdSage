import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';
import tailwindcss from '@tailwindcss/vite';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "crowdsage.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:5017';

// Where dev requests are forwarded. When SpaProxy launches this from `dotnet run`
// it sets ASPNETCORE_*, so `target` already points at that server. Run standalone
// (`npm run dev` against the Docker backend) neither is set, so default to 8080.
const backendTarget = env.ASPNETCORE_HTTPS_PORT || env.ASPNETCORE_URLS
    ? target
    : 'http://localhost:8080';

// VITE_CROWDSAGE_BACKEND_URL is `/api` in every build, so the API, the Identity
// register action and the OpenIddict token endpoint are all same-origin in dev and
// must be forwarded. `/register` and `/connect/token` are mapped at the server root
// (serverRootUrl is '' once `/api` is stripped), so they need rules of their own.
const backendProxy = {
    target: backendTarget,
    secure: false,
    changeOrigin: true,
};

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), tailwindcss(),],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': backendProxy,
            '^/register$': backendProxy,
            '^/connect/': backendProxy,
            '^/weatherforecast': {
                target,
                secure: false
            }
        },
        port: 51708,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})
