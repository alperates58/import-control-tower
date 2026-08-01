import React, { useState, useRef, useEffect } from 'react';
import { IconButton } from './Button';

export interface DropdownItemProps {
  label: React.ReactNode;
  icon?: React.ReactNode;
  onClick: () => void;
  isDanger?: boolean;
}

export interface DropdownMenuProps {
  items: DropdownItemProps[];
  trigger?: React.ReactNode;
}

export const DropdownMenu: React.FC<DropdownMenuProps> = ({ items, trigger }) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  return (
    <div className="dropdown-container" ref={containerRef}>
      <div onClick={() => setIsOpen(!isOpen)}>
        {trigger || (
          <IconButton
            icon={
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="1" />
                <circle cx="12" cy="5" r="1" />
                <circle cx="12" cy="19" r="1" />
              </svg>
            }
            aria-label="İşlemler Menüsü"
          />
        )}
      </div>

      {isOpen && (
        <div className="dropdown-menu">
          {items.map((item, idx) => (
            <button
              key={idx}
              className={`dropdown-item ${item.isDanger ? 'danger' : ''}`}
              onClick={() => {
                setIsOpen(false);
                item.onClick();
              }}
            >
              {item.icon && <span>{item.icon}</span>}
              <span>{item.label}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
