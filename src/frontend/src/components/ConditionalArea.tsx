import { HTMLAttributes, PropsWithChildren } from "react";

type Props = PropsWithChildren<HTMLAttributes<HTMLDivElement>>

export default function ConditionalArea(props: Props)
{    
    return <div className="govuk-radios__conditional" {...props}>
        {props.children}
    </div>;
}