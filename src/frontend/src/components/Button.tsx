import { ButtonHTMLAttributes, useCallback } from "react";
import { initializeNode } from "./IdSkModule";

interface IProps extends ButtonHTMLAttributes<HTMLButtonElement>
{
    buttonType: 'primary' | 'secondary' | 'warning';
}

export default function Button(props: IProps)
{
    const initialize = useCallback((node: HTMLButtonElement) => {
        initializeNode(node);
    }, []);

    const {buttonType, ...buttonProperties} = props;
    let className = 'idsk-button';

    switch (buttonType)
    {
        case 'secondary':
            className += ' idsk-button--secondary';
            break;
        case 'warning':
            className += ' idsk-button--warning';
            break;
    }

    return <button ref={initialize} className={className} data-module="idsk-button" {...buttonProperties}>
        {props.children}
    </button>;
}

Button.defaultProps = {
    buttonType: 'primary'
}