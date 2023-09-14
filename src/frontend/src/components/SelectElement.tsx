import { SelectHTMLAttributes } from "react";

interface IProps extends SelectHTMLAttributes<HTMLSelectElement>
{

}

export default function SelectElement(props: IProps)
{
    return <select className="govuk-select" {...props}>
        {props.children}
    </select>;
}