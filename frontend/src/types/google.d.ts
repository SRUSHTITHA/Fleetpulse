// Minimal ambient types for Google Identity Services (accounts.google.com/gsi/client).
interface CredentialResponse {
  credential: string;
  select_by?: string;
}

interface PromptMomentNotification {
  isDisplayMoment: () => boolean;
  isDisplayed: () => boolean;
  isNotDisplayed: () => boolean;
  isSkippedMoment: () => boolean;
  isDismissedMoment: () => boolean;
  getNotDisplayedReason: () => string;
  getSkippedReason: () => string;
  getDismissedReason: () => string;
}

interface GoogleIdentityClient {
  initialize(config: {
    client_id: string;
    callback: (response: CredentialResponse) => void;
    ux_mode?: 'popup' | 'redirect';
    auto_select?: boolean;
  }): void;
  renderButton(element: HTMLElement, options?: Record<string, unknown>): void;
  prompt(callback?: (notification: PromptMomentNotification) => void): void;
}

interface Window {
  google?: {
    accounts: {
      id: GoogleIdentityClient;
    };
  };
}

interface ImportMetaEnv {
  readonly VITE_GOOGLE_AUTH_CLIENT_ID?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
