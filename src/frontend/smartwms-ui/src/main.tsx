import { InMemoryWebStorage, WebStorageStateStore } from "oidc-client-ts";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import React from "react";
import ReactDOM from "react-dom/client";
import { AuthProvider } from "react-oidc-context";
import { BrowserRouter } from "react-router-dom";
import App from "./App.tsx";
import { cognitoAuthConfig } from "./auth/cognitoAuth";
import "./styles/app.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false
    }
  }
});

const inMemoryUserStore = new WebStorageStateStore({ store: new InMemoryWebStorage() });
const sessionStateStore = new WebStorageStateStore({ store: window.sessionStorage });

function handleSigninCallback() {
  const cleanPath = `${window.location.origin}${window.location.pathname}`;
  window.history.replaceState({}, document.title, cleanPath);
}

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AuthProvider
      {...cognitoAuthConfig}
      userStore={inMemoryUserStore}
      stateStore={sessionStateStore}
      onSigninCallback={handleSigninCallback}
    >
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    </AuthProvider>
  </React.StrictMode>
);
