import { InputHTMLAttributes, forwardRef, ForwardedRef } from "react";

type Props = InputHTMLAttributes<HTMLInputElement>;

const FileUpload = forwardRef((props: Props, ref: ForwardedRef<HTMLInputElement>) => {
    return <input ref={ref} className="govuk-file-upload" type="file" {...props} />;
});

export default FileUpload;
