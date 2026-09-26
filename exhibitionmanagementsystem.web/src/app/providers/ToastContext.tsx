import React, { createContext, useContext, useState, useCallback } from 'react';
import { CheckCircle2, AlertTriangle, AlertCircle, Info, X } from 'lucide-react';

export type ToastType = 'success' | 'danger' | 'warning' | 'info';

export interface ToastItem {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastContextType {
  showToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  const showToast = useCallback((message: string, type: ToastType = 'info') => {
    const id = Math.random().toString(36).substring(2, 9);
    setToasts(prev => [...prev, { id, message, type }]);

    setTimeout(() => {
      removeToast(id);
    }, 4500);
  }, [removeToast]);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      {/* Toast Overlay Host */}
      <div
        style={{
          position: 'fixed',
          top: '24px',
          left: '24px',
          zIndex: 9999,
          display: 'flex',
          flexDirection: 'column',
          gap: '12px',
          maxWidth: '400px',
          pointerEvents: 'none'
        }}
      >
        {toasts.map(toast => {
          const isSuccess = toast.type === 'success';
          const isDanger = toast.type === 'danger';
          const isWarning = toast.type === 'warning';

          const bg = isSuccess ? 'var(--success-bg)' : isDanger ? 'var(--danger-bg)' : isWarning ? 'var(--warning-bg)' : 'var(--info-bg)';
          const text = isSuccess ? 'var(--success-text)' : isDanger ? 'var(--danger-text)' : isWarning ? 'var(--warning-text)' : 'var(--info-text)';
          const border = isSuccess ? '#10b981' : isDanger ? '#ef4444' : isWarning ? '#f59e0b' : '#3b82f6';

          return (
            <div
              key={toast.id}
              style={{
                pointerEvents: 'auto',
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
                padding: '12px 18px',
                background: '#ffffff',
                borderRight: `4px solid ${border}`,
                borderRadius: 'var(--radius-lg)',
                boxShadow: 'var(--shadow-lg)',
                color: 'var(--text-primary)',
                fontSize: '14px',
                fontWeight: 500,
                animation: 'slideIn 0.3s ease-out',
                minWidth: '280px'
              }}
            >
              <div
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: 'var(--radius-md)',
                  background: bg,
                  color: text,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0
                }}
              >
                {isSuccess && <CheckCircle2 size={18} />}
                {isDanger && <AlertCircle size={18} />}
                {isWarning && <AlertTriangle size={18} />}
                {toast.type === 'info' && <Info size={18} />}
              </div>

              <div style={{ flex: 1 }}>{toast.message}</div>

              <button
                onClick={() => removeToast(toast.id)}
                style={{
                  color: 'var(--text-muted)',
                  display: 'flex',
                  alignItems: 'center',
                  padding: '4px',
                  borderRadius: 'var(--radius-sm)'
                }}
              >
                <X size={16} />
              </button>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
};

// Provider and hook intentionally share one public module for stable imports.
// eslint-disable-next-line react-refresh/only-export-components
export const useToast = () => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
};
