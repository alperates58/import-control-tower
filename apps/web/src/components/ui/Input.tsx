import React from 'react';

export interface FormFieldProps {
  label?: string;
  required?: boolean;
  helpText?: string;
  errorText?: string;
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
}

export const FormField: React.FC<FormFieldProps> = ({
  label,
  required,
  helpText,
  errorText,
  children,
  className = '',
  style
}) => (
  <div className={`form-field ${className}`.trim()} style={style}>
    {label && (
      <label className={`form-label ${required ? 'required' : ''}`}>
        {label}
      </label>
    )}
    {children}
    {errorText ? (
      <span className="form-error-text">{errorText}</span>
    ) : helpText ? (
      <span className="form-help-text">{helpText}</span>
    ) : null}
  </div>
);

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className = '', error, ...props }, ref) => (
    <input
      ref={ref}
      className={`form-input ${error ? 'border-rose' : ''} ${className}`.trim()}
      {...props}
    />
  )
);

Input.displayName = 'Input';

export const SearchInput: React.FC<InputProps> = (props) => (
  <div style={{ position: 'relative', width: '100%' }}>
    <Input
      {...props}
      style={{ paddingLeft: '2.2rem', ...props.style }}
    />
    <svg
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      style={{
        position: 'absolute',
        left: '0.75rem',
        top: '50%',
        transform: 'translateY(-50%)',
        color: 'var(--text-dim)',
        pointerEvents: 'none'
      }}
    >
      <circle cx="11" cy="11" r="8" />
      <line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
  </div>
);

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  options?: { value: string; label: string }[];
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ children, options, className = '', ...props }, ref) => (
    <select ref={ref} className={`form-select ${className}`.trim()} {...props}>
      {options
        ? options.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))
        : children}
    </select>
  )
);

Select.displayName = 'Select';

export const Textarea = React.forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className = '', ...props }, ref) => (
    <textarea ref={ref} className={`form-textarea ${className}`.trim()} {...props} />
  )
);

Textarea.displayName = 'Textarea';

export const Checkbox: React.FC<React.InputHTMLAttributes<HTMLInputElement> & { label?: string }> = ({
  label,
  className = '',
  ...props
}) => (
  <label style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: 'var(--font-sm)', color: 'var(--text-secondary)' }}>
    <input type="checkbox" style={{ accentColor: 'var(--primary)', cursor: 'pointer' }} {...props} />
    {label && <span>{label}</span>}
  </label>
);
