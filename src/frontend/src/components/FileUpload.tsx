import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLInputElement>
{
    label: string;
}

export default function Radio(props: IProps)
{
    return <input className="govuk-file-upload" type="file" {...props} />
}