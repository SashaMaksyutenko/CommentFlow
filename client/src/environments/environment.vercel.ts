// Used by the Vercel build (ng build --configuration production,vercel) instead of environment.ts.
// The page is on Vercel and the API is on Render, so every address is a full one.
// The API must list the Vercel address in Cors:AllowedOrigins.
const apiOrigin = 'https://commentflow-api.onrender.com';

export const environment = {
  apiUrl: `${apiOrigin}/api`,
  hubUrl: `${apiOrigin}/hubs/comments`,
  filesUrl: apiOrigin,
};
