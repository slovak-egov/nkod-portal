import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLDivElement>
{
    
}

export default function MainContent(props: IProps)
{
    return <main className="govuk-main-wrapper govuk-main-wrapper--auto-spacing" {...props}>
        {props.children}
    </main>;
}