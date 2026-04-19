/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_INVENTORY_API_ORIGIN?: string;
  readonly VITE_COGNITO_AUTHORITY?: string;
  readonly VITE_COGNITO_CLIENT_ID?: string;
  readonly VITE_COGNITO_DOMAIN?: string;
  readonly VITE_COGNITO_REDIRECT_URI?: string;
  readonly VITE_COGNITO_POST_LOGOUT_REDIRECT_URI?: string;
  readonly VITE_COGNITO_SCOPE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
