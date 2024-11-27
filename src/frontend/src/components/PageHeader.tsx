import { PropsWithChildren } from 'react';

export type Props = {
    size?: 's' | 'm' | 'l' | 'xl';
} & PropsWithChildren;

export default function PageHeader(props: Props) {
    const size = props.size ?? 'xl';
    return <h1 className={`govuk-heading-${size}`}>{props.children}</h1>;
}
