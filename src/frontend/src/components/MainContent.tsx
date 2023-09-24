import { HTMLAttributes } from "react";

type Props = HTMLAttributes<HTMLDivElement>

export default function MainContent(props: Props)
{
    return <main className="govuk-main-wrapper govuk-main-wrapper--auto-spacing" {...props}>
        {props.children}
    </main>;
}