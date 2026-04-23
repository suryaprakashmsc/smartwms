export const missingCognitoConfigKeys = [
  ["VITE_COGNITO_REGION", import.meta.env.VITE_COGNITO_REGION],
  ["VITE_COGNITO_USER_POOL_ID", import.meta.env.VITE_COGNITO_USER_POOL_ID],
  ["VITE_COGNITO_CLIENT_ID", import.meta.env.VITE_COGNITO_CLIENT_ID],
  ["VITE_COGNITO_DOMAIN", import.meta.env.VITE_COGNITO_DOMAIN]
]
  .filter(([, value]) => !value)
  .map(([key]) => key)
