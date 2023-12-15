import { ButtonHTMLAttributes, useCallback } from 'react';
import { initializeNode } from './IdSkModule';

type Props = {
    buttonType: 'primary' | 'secondary' | 'warning';
} & ButtonHTMLAttributes<HTMLButtonElement>;

export default function Button(props: Props) {
    const initialize = useCallback((node: HTMLButtonElement) => {
        if (node == null) return;
        initializeNode(node);
    }, []);

    const { buttonType, className, ...buttonProperties } = props;
    let effectiveClassName = 'idsk-button';

    switch (buttonType) {
        case 'secondary':
            effectiveClassName += ' idsk-button--secondary';
            break;
        case 'warning':
            effectiveClassName += ' idsk-button--warning';
            break;
    }

    effectiveClassName += ' ' + className;

    return (
        <button ref={initialize} className={effectiveClassName} data-module="idsk-button" {...buttonProperties}>
            {props.children}
        </button>
    );
}

Button.defaultProps = {
    buttonType: 'primary'
};
