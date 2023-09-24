import { InputHTMLAttributes } from "react";

type Props = InputHTMLAttributes<HTMLInputElement>

export default function FileUpload(props: Props)
{
    return <input className="govuk-file-upload" type="file" {...props} />
}