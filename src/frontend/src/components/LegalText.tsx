import { PropsWithChildren, ReactNode } from 'react';

export function P(props: PropsWithChildren) {
    return <p className="govuk-body">{props.children}</p>;
}

export function ExternalLink(props: { href: string; children?: ReactNode }) {
    return (
        <a className="govuk-link" href={props.href}>
            {props.children ?? props.href}
        </a>
    );
}
