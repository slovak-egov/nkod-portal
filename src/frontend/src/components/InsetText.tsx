import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLDivElement>

export default function InsetText(props: Props)
{
    return <div className="govuk-inset-text" {...props}>
        {props.children}
    </div>;
}