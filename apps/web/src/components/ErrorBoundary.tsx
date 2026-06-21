import { Component, type ErrorInfo, type ReactNode } from "react";
import { uz } from "@/content/uz";

interface Props {
  children: ReactNode;
}
interface State {
  error: Error | null;
  info: ErrorInfo | null;
}

/**
 * Top-level render-error boundary. Catches any uncaught error thrown while rendering
 * the wrapped subtree and shows a recoverable fallback (rule 11 - vetted Uzbek copy
 * from the content store) instead of leaving the user with a blank screen.
 *
 * Mounted once at the app root (main.tsx) around the router, and additionally around a
 * handful of pages that fetch/render complex nested data, for finer-grained recovery.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null, info: null };

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    this.setState({ error, info });
    // eslint-disable-next-line no-console
    console.error("ErrorBoundary caught:", error, info.componentStack);
  }

  reset = () => {
    this.setState({ error: null, info: null });
  };

  render() {
    if (this.state.error) {
      return (
        <div
          role="alert"
          style={{
            minHeight: "100vh",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            textAlign: "center",
            padding: "24px",
            color: "#ffffff",
          }}
        >
          <p
            style={{
              fontFamily: "inherit",
              fontSize: 20,
              fontWeight: 700,
              lineHeight: 1.5,
              maxWidth: 420,
              margin: 0,
              marginBottom: 24,
            }}
          >
            {uz.common.error}
          </p>
          <button
            type="button"
            onClick={() => this.reset()}
            style={{
              fontFamily: "inherit",
              fontWeight: 700,
              fontSize: 16,
              color: "#10150c",
              background: "#b9d7aa",
              border: "none",
              borderRadius: 16,
              padding: "12px 28px",
              cursor: "pointer",
            }}
          >
            {uz.common.retry}
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
