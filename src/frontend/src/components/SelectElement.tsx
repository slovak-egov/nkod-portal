import { SelectHTMLAttributes } from "react";

type Props = SelectHTMLAttributes<HTMLSelectElement>

export default function SelectElement(props: Props)
{
    return <select className="govuk-select" {...props}>
        {props.children}
    </select>;
}