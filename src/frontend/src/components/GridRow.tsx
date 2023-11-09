import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLDivElement>;

export default function GridRow(props: Props)
{
    const { className, children, ...rest } = props;

    return <div className={'govuk-grid-row ' + className} {...rest}>
        {children}
    </div>
}