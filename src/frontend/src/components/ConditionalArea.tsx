import { HTMLAttributes, PropsWithChildren } from "react";

interface IProps extends PropsWithChildren<HTMLAttributes<HTMLDivElement>>
{
    
}

export default function ConditionalArea(props: IProps)
{    
    return <div className="govuk-radios__conditional" {...props}>
        {props.children}
    </div>;
}