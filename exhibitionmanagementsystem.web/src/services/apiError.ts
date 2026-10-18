import axios from 'axios';

export const getApiErrorMessage = (error: unknown, fallback: string): string => {
  if (!axios.isAxiosError(error)) return fallback;

  const body: unknown = error.response?.data;
  if (typeof body === 'string' && body.trim()) return body;
  if (body && typeof body === 'object') {
    const errorMessage = 'errorMessage' in body ? body.errorMessage : undefined;
    if (typeof errorMessage === 'string' && errorMessage.trim()) return errorMessage;

    const message = 'message' in body ? body.message : undefined;
    if (typeof message === 'string' && message.trim()) return message;

    const title = 'title' in body ? body.title : undefined;
    if (typeof title === 'string' && title.trim()) return title;
  }

  return fallback;
};
