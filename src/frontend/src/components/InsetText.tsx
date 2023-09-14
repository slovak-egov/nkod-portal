import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLDivElement>
{
    
}

export default function InsetText(props: IProps)
{
    return <div className="govuk-inset-text" {...props}>
        {props.children}
    </div>;
}