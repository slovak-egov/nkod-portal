import { HTMLAttributes, useCallback } from "react";

//@ts-ignore
import { initAll } from  '@id-sk/frontend/idsk/all';

export function initializeNode<T extends HTMLElement>(node: T)
{
    if (node == null) return;
    initAll({
        scope: node.parentNode
    });
}

interface IProps extends HTMLAttributes<HTMLDivElement>
{
    moduleType: string;
}

export default function IdSkModule(props: IProps) 
{
    const initialize = useCallback((node: HTMLDivElement) => {
        initializeNode(node);
    }, []);

    const {moduleType, ...divProperties} = props;

    return <div ref={initialize} data-module={moduleType} {...divProperties}>
            {props.children}
        </div>;
}