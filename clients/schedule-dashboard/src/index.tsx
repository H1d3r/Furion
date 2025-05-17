import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import App from "./App";
import apiconfig from "./components/jobs/apiconfig";
import "./index.css";
import Login from "./login";
import { AuthProvider, RequireAuth } from "./auth";

const root = ReactDOM.createRoot(
  document.getElementById("root") as HTMLElement
);
root.render(
  <React.StrictMode>
    <BrowserRouter basename={apiconfig.requestPath}>
      <AuthProvider>
        <Routes>
          <Route
            index
            element={
              <RequireAuth>
                <App />
              </RequireAuth>
            }
          />
          <Route path="/login" element={<Login />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
