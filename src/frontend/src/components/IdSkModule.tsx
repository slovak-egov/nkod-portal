import { HTMLAttributes, useCallback, useEffect, useState } from 'react';

//@ts-ignore
import { initAll } from '@id-sk/frontend/idsk/all';
import { UserInfo } from '../client';

export function initializeNode<T extends HTMLElement>(node: T) {
    if (node == null) return;
    initAll({
        scope: node.parentNode
    });
}

type Props = {
    moduleType: string;
    userInfo?: UserInfo;
} & HTMLAttributes<HTMLDivElement>;

export default function IdSkModule(props: Props) {
    const [node, setNode] = useState<HTMLDivElement | null>(null);

    const { moduleType, userInfo, ...divProperties } = props;

    const initialize = useCallback(
        (node: HTMLDivElement) => {
            setNode(node);
        },
        [moduleType]
    );

    useEffect(() => {
        if (node != null) {
            initializeNode(node);
        }
    }, [userInfo, node]);

    return (
        <div>
            <div ref={initialize} data-module={moduleType} {...divProperties}>
                {props.children}
            </div>
        </div>
    );
}
