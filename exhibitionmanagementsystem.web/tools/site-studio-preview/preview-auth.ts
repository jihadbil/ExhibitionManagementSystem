// The preview has a dedicated fixture identity and no connection to real authentication.
export const useAuth = () => ({ user: { tenantID: -1, userId: 'site-studio-demo' } });
