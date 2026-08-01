import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { ThemeProvider, useTheme } from '../context/ThemeContext';
import { ThemeToggle } from '../components/ui/ThemeToggle';

const TestThemeComponent: React.FC = () => {
  const { theme, effectiveTheme } = useTheme();
  return (
    <div>
      <span data-testid="current-theme">{theme}</span>
      <span data-testid="effective-theme">{effectiveTheme}</span>
      <ThemeToggle />
    </div>
  );
};

describe('Theme System Unit Suite', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('1. Defaults to system theme and reads prefers-color-scheme fallback', () => {
    render(
      <ThemeProvider>
        <TestThemeComponent />
      </ThemeProvider>
    );

    expect(screen.getByTestId('current-theme').textContent).toBe('system');
    expect(document.documentElement.getAttribute('data-theme')).toBeTruthy();
  });

  it('2. Dark theme selection updates attribute and localStorage', () => {
    render(
      <ThemeProvider>
        <TestThemeComponent />
      </ThemeProvider>
    );

    const darkButton = screen.getByLabelText('Karanlık Tema');
    fireEvent.click(darkButton);

    expect(screen.getByTestId('current-theme').textContent).toBe('dark');
    expect(screen.getByTestId('effective-theme').textContent).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('ict_theme')).toBe('dark');
  });

  it('3. Light theme selection updates attribute and localStorage', () => {
    render(
      <ThemeProvider>
        <TestThemeComponent />
      </ThemeProvider>
    );

    const lightButton = screen.getByLabelText('Açık Tema');
    fireEvent.click(lightButton);

    expect(screen.getByTestId('current-theme').textContent).toBe('light');
    expect(screen.getByTestId('effective-theme').textContent).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('ict_theme')).toBe('light');
  });

  it('4. Restores saved theme from localStorage on initial render', () => {
    localStorage.setItem('ict_theme', 'light');

    render(
      <ThemeProvider>
        <TestThemeComponent />
      </ThemeProvider>
    );

    expect(screen.getByTestId('current-theme').textContent).toBe('light');
    expect(screen.getByTestId('effective-theme').textContent).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('5. System theme option responds to theme selector toggle', () => {
    render(
      <ThemeProvider>
        <TestThemeComponent />
      </ThemeProvider>
    );

    // Switch to dark
    fireEvent.click(screen.getByLabelText('Karanlık Tema'));
    expect(screen.getByTestId('current-theme').textContent).toBe('dark');

    // Switch back to system
    fireEvent.click(screen.getByLabelText('Sistem Teması'));
    expect(screen.getByTestId('current-theme').textContent).toBe('system');
    expect(localStorage.getItem('ict_theme')).toBe('system');
  });
});
