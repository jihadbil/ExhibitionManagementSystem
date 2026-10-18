import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ToastProvider } from '../../src/app/providers/ToastContext';
import { BoothDesignerPage } from '../../src/features/booths/pages/BoothDesignerPage';
import { VenueDesignerPage } from '../../src/features/venues/pages/VenueDesignerPage';
import '../../src/styles/index.css';
import './preview.css';
createRoot(document.getElementById('root')!).render(<React.StrictMode><ToastProvider><BrowserRouter><Routes><Route path="/venues/:venueId/designer" element={<VenueDesignerPage/>}/><Route path="/booths/designer" element={<BoothDesignerPage/>}/><Route path="*" element={<Navigate to="/venues/1/designer" replace/>}/></Routes></BrowserRouter></ToastProvider></React.StrictMode>);
